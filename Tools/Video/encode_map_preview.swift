import Foundation
import AVFoundation
import CoreGraphics
import ImageIO

// Encode Unity's numbered preview frames without changing their pixels or timing.
let folder = URL(fileURLWithPath: CommandLine.arguments[1])
let output = URL(fileURLWithPath: CommandLine.arguments[2])
let count = Int(CommandLine.arguments[3])!
let fps = Int32(CommandLine.arguments[4])!
let width = 1280, height = 720
if FileManager.default.fileExists(atPath: output.path) { try FileManager.default.removeItem(at: output) }
let writer = try AVAssetWriter(outputURL: output, fileType: .mp4)
let input = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: width, AVVideoHeightKey: height,
    AVVideoCompressionPropertiesKey: [AVVideoAverageBitRateKey: 8_000_000,
                                     AVVideoMaxKeyFrameIntervalKey: Int(fps)]
])
input.expectsMediaDataInRealTime = false
let adapter = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: input, sourcePixelBufferAttributes: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
    kCVPixelBufferWidthKey as String: width, kCVPixelBufferHeightKey as String: height,
    kCVPixelBufferCGImageCompatibilityKey as String: true,
    kCVPixelBufferCGBitmapContextCompatibilityKey as String: true
])
writer.add(input)
guard writer.startWriting() else { fatalError(String(describing: writer.error)) }
writer.startSession(atSourceTime: .zero)
for i in 0..<count {
    while !input.isReadyForMoreMediaData {
        if writer.status == .failed { fatalError(String(describing: writer.error)) }
        Thread.sleep(forTimeInterval: 0.002)
    }
    try autoreleasepool {
        let path = folder.appendingPathComponent(String(format: "%03d.png", i))
        guard let source = CGImageSourceCreateWithURL(path as CFURL, nil),
              let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
            throw NSError(domain: "Preview", code: 1, userInfo: [NSLocalizedDescriptionKey: path.path])
        }
        var buffer: CVPixelBuffer?
        guard CVPixelBufferPoolCreatePixelBuffer(nil, adapter.pixelBufferPool!, &buffer) == kCVReturnSuccess else { fatalError("Buffer allocation failed") }
        let pixels = buffer!
        CVPixelBufferLockBaseAddress(pixels, [])
        let context = CGContext(data: CVPixelBufferGetBaseAddress(pixels), width: width, height: height,
            bitsPerComponent: 8, bytesPerRow: CVPixelBufferGetBytesPerRow(pixels), space: CGColorSpaceCreateDeviceRGB(),
            bitmapInfo: CGImageAlphaInfo.noneSkipFirst.rawValue)!
        context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
        CVPixelBufferUnlockBaseAddress(pixels, [])
        guard adapter.append(pixels, withPresentationTime: CMTime(value: Int64(i), timescale: fps)) else {
            fatalError(String(describing: writer.error))
        }
    }
}
input.markAsFinished()
let done = DispatchSemaphore(value: 0)
writer.finishWriting { done.signal() }
done.wait()
guard writer.status == .completed else { fatalError(String(describing: writer.error)) }
print("Encoded \(count) frames at \(fps) fps: \(output.path)")
