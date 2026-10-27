// Extract source 0.5–2.0 seconds and normalize this lever SFX to -1 dBFS.
import Foundation
import AVFoundation
import CoreMedia

let args = CommandLine.arguments
let source = URL(fileURLWithPath: args.count > 1 ? args[1] : "/Users/nurxayot/Desktop/railway.mp4")
let destination = URL(fileURLWithPath: args.count > 2 ? args[2] : "Assets/Audio/UI/RailwayLeverMechanism.wav")
let asset = AVURLAsset(url: source)
guard let track = asset.tracks(withMediaType: .audio).first else { fatalError("The video has no audio track") }
let reader = try AVAssetReader(asset: asset)
reader.timeRange = CMTimeRange(start: .zero, duration: CMTime(seconds: 2.15, preferredTimescale: 48000))
let output = AVAssetReaderTrackOutput(track: track, outputSettings: [
    AVFormatIDKey: kAudioFormatLinearPCM,
    AVSampleRateKey: 48000,
    AVNumberOfChannelsKey: 2,
    AVLinearPCMBitDepthKey: 16,
    AVLinearPCMIsFloatKey: false,
    AVLinearPCMIsBigEndianKey: false,
    AVLinearPCMIsNonInterleaved: false
])
output.alwaysCopiesSampleData = false
reader.add(output)
guard reader.startReading() else { throw reader.error ?? NSError(domain: "LeverAudio", code: 1) }
let wantedBytes = 48000 * 2 * 2 * 2
var pcm = Data(repeating: 0, count: wantedBytes)
var copiedBytes = 0
var coveredEnd = 0
while let sample = output.copyNextSampleBuffer() {
    guard let block = CMSampleBufferGetDataBuffer(sample) else { fatalError("Missing audio buffer") }
    let count = CMBlockBufferGetDataLength(block)
    var bytes = [UInt8](repeating: 0, count: count)
    let status = CMBlockBufferCopyDataBytes(block, atOffset: 0, dataLength: count, destination: &bytes)
    guard status == kCMBlockBufferNoErr else { fatalError("Audio copy failed") }
    let frame = Int((CMTimeGetSeconds(CMSampleBufferGetPresentationTimeStamp(sample))*48000).rounded())
    let byteStart = frame * 4
    let sourceStart = max(0, -byteStart)
    let destinationStart = max(0, byteStart)
    let length = min(count-sourceStart, wantedBytes-destinationStart)
    if length > 0 {
        pcm.replaceSubrange(destinationStart..<(destinationStart+length), with: bytes[sourceStart..<(sourceStart+length)])
        copiedBytes += length
        coveredEnd = max(coveredEnd, destinationStart+length)
    }
}
guard reader.status == .completed else { fatalError(reader.error?.localizedDescription ?? "Audio decode incomplete") }
guard copiedBytes > 0 && CMTimeGetSeconds(asset.duration) >= 2 else {
    throw NSError(domain: "LeverAudio", code: 2, userInfo: [NSLocalizedDescriptionKey: "Source shorter than two seconds or no audio"])
}
print("Decoded audio bytes: \(copiedBytes); covered endpoint: \(Double(coveredEnd)/192000) seconds")
// Select the requested interval, then apply uniform gain without changing timing or pitch.
pcm = Data(pcm[(48000 * 4 / 2)..<wantedBytes])
let raw = [UInt8](pcm)
var peak = 0
for i in stride(from: 0, to: raw.count, by: 2) {
    peak = max(peak, abs(Int(Int16(bitPattern: UInt16(raw[i]) | UInt16(raw[i+1]) << 8))))
}
guard peak > 0 else { throw NSError(domain: "LeverAudio", code: 3) }
let gain = 29204.0 / Double(peak)
if abs(gain-1) > 0.000001 {
    var normalized = Data(capacity: pcm.count)
    for i in stride(from: 0, to: raw.count, by: 2) {
        let sample = Int16(bitPattern: UInt16(raw[i]) | UInt16(raw[i+1]) << 8)
        var value = Int16((Double(sample)*gain).rounded()).littleEndian
        withUnsafeBytes(of: &value) { normalized.append(contentsOf: $0) }
    }
    pcm = normalized
}
print("Uniform gain: \(20*log10(gain)) dB")
var wav = Data()
func tag(_ s: String) { wav.append(contentsOf: s.utf8) }
func u16(_ n: UInt16) { var x = n.littleEndian; withUnsafeBytes(of: &x) { wav.append(contentsOf: $0) } }
func u32(_ n: UInt32) { var x = n.littleEndian; withUnsafeBytes(of: &x) { wav.append(contentsOf: $0) } }
tag("RIFF"); u32(UInt32(36 + pcm.count)); tag("WAVE"); tag("fmt "); u32(16)
u16(1); u16(2); u32(48000); u32(48000 * 4); u16(4); u16(16)
tag("data"); u32(UInt32(pcm.count)); wav.append(pcm)
try wav.write(to: destination, options: .atomic)
print("Saved \(destination.path): source 00:00.500–00:02.000, 48 kHz stereo 16-bit PCM")
