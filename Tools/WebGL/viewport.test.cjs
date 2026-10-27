const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const templatePath = path.join(__dirname,
  '../../Assets/WebGLTemplates/LuxoddTemplate/index.html.template');
const template = fs.readFileSync(templatePath, 'utf8');
const viewportScript = template.match(/<script>([\s\S]*?)<\/script>/)[1];

test('build-time template and generated page use the same viewport code', () => {
  const page = fs.readFileSync(templatePath.replace(/\.template$/, ''), 'utf8');
  assert.equal(page.match(/<script>([\s\S]*?)<\/script>/)[1], viewportScript);
});

function fixture(width, height, dpr = 1, visualViewport = true) {
  const events = new Map();
  const queries = [];
  const timers = [];
  const canvas = { style: {}, writes: 0 };
  for (const dimension of ['width', 'height']) {
    let value = 0;
    Object.defineProperty(canvas, dimension, {
      get: () => value,
      set: next => { value = next; canvas.writes++; },
    });
  }
  const container = { style: {} };
  const listen = target => (name, handler) => events.set(`${target}:${name}`, handler);
  const window = {
    innerWidth: width, innerHeight: height, devicePixelRatio: dpr,
    addEventListener: listen('window'),
    matchMedia: media => {
      const query = { media, addEventListener: (name, fn, options) => {
        assert.equal(name, 'change');
        assert.equal(options.once, true);
        query.change = fn;
      } };
      queries.push(query);
      return query;
    },
  };
  if (visualViewport) window.visualViewport = {
    width, height, offsetLeft: 0, offsetTop: 0,
    addEventListener: listen('visualViewport'),
  };
  const document = {
    querySelector: selector => selector === '#unity-canvas' ? canvas : container,
    addEventListener: listen('document'),
  };
  const context = vm.createContext({ window, document, setTimeout: fn => timers.push(fn) });
  vm.runInContext(viewportScript, context);
  return { window, canvas, container, events, queries, timers, context };
}

for (const [name, width, height, dpr, expectedW, expectedH] of [
  ['1080p arcade', 1920, 1080, 1, 1920, 1080],
  ['4K arcade', 3840, 2160, 1, 3840, 2160],
  ['4K with 200% display scaling', 1920, 1080, 2, 3840, 2160],
  ['1440p with 125% display scaling', 2048, 1152, 1.25, 2560, 1440],
  ['ultrawide arcade', 3440, 1440, 1, 3440, 1440],
  ['portrait arcade', 2160, 3840, 1, 2160, 3840],
  ['small embedded player', 960, 540, 1, 960, 540],
]) {
  test(`${name}: render at physical pixels and retain CSS input dimensions`, () => {
    const { context, canvas, container } = fixture(width, height, dpr);
    context.LuxoddApplyVisualViewportSize();
    assert.equal(canvas.width, expectedW);
    assert.equal(canvas.height, expectedH);
    assert.equal(canvas.style.width, `${width}px`);
    assert.equal(canvas.style.height, `${height}px`);
    assert.equal(container.style.width, `${width}px`);
  });
}

test('fractional visual viewport covers the display and preserves its offset', () => {
  const { context, window, canvas, container } = fixture(1000.25, 600.5, 1.25);
  window.visualViewport.offsetLeft = 2.8;
  window.visualViewport.offsetTop = 3.4;
  context.LuxoddApplyVisualViewportSize();
  assert.equal(canvas.style.width, '1001px');
  assert.equal(canvas.style.height, '601px');
  assert.equal(canvas.width, 1251);
  assert.equal(canvas.height, 751);
  assert.equal(container.style.left, '2px');
  assert.equal(container.style.top, '3px');
});

test('browser without visualViewport uses the window dimensions', () => {
  const { context, canvas } = fixture(2560, 1440, 1, false);
  context.LuxoddApplyVisualViewportSize();
  assert.equal(canvas.width, 2560);
  assert.equal(canvas.height, 1440);
});

test('DPR-only changes resize the backing buffer and re-arm the listener', () => {
  const { context, window, canvas, queries } = fixture(1920, 1080);
  context.LuxoddInstallViewportHandlers();
  window.devicePixelRatio = 2;
  queries[0].change();
  assert.equal(canvas.width, 3840);
  assert.equal(canvas.height, 2160);
  assert.equal(queries[1].media, '(resolution: 2dppx)');
  window.devicePixelRatio = 1;
  queries[1].change();
  assert.equal(canvas.width, 1920);
  assert.equal(queries.length, 3);
});

test('fullscreen and orientation changes update resolution without duplicate installation', () => {
  const { context, window, canvas, events, queries } = fixture(1920, 1080);
  context.LuxoddInstallViewportHandlers();
  context.LuxoddInstallViewportHandlers();
  assert.equal(queries.length, 1);
  window.visualViewport.width = 3840;
  window.visualViewport.height = 2160;
  events.get('document:fullscreenchange')();
  assert.equal(canvas.width, 3840);
  window.visualViewport.width = 2160;
  window.visualViewport.height = 3840;
  events.get('window:orientationchange')();
  assert.equal(canvas.width, 2160);
  assert.equal(canvas.height, 3840);
});

test('unchanged resize events do not reset the canvas drawing buffer', () => {
  const { context, canvas, events, timers } = fixture(1920, 1080, 2);
  context.LuxoddInstallViewportHandlers();
  const writes = canvas.writes;
  for (const handler of events.values()) handler();
  for (const timer of timers) timer();
  context.LuxoddForceApplyViewportAfterUnityStart();
  assert.equal(canvas.writes, writes);
});
