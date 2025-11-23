const ImageSimilarity = require('./src/main/imageSimilarity');

console.log('Testing ImageSimilarity module...\n');

const similarity = new ImageSimilarity(0.05);

console.log('Initial stats:', similarity.getStats());

// Create test buffers with realistic variation
const testBuffer1 = Buffer.alloc(10000);
for (let i = 0; i < testBuffer1.length; i++) {
  testBuffer1[i] = Math.floor(Math.random() * 256);
}

// Nearly identical (simulating JPEG compression artifacts)
const testBuffer2 = Buffer.from(testBuffer1);
for (let i = 0; i < testBuffer2.length; i += 100) {
  testBuffer2[i] = (testBuffer2[i] + Math.floor(Math.random() * 3)) % 256;
}

// Very different buffer
const testBuffer3 = Buffer.alloc(10000);
for (let i = 0; i < testBuffer3.length; i++) {
  testBuffer3[i] = Math.floor(Math.random() * 256);
}

const metadata = { width: 640, height: 360 };

console.log('\nTest 1: First frame (should send)');
const result1 = similarity.shouldSendFrame(testBuffer1, metadata);
console.log('Result:', result1, '(expected: true)\n');

console.log('\nTest 2: Nearly identical with minor artifacts (should NOT send)');
const result2 = similarity.shouldSendFrame(testBuffer2, metadata);
console.log('Result:', result2, '(expected: false)\n');

console.log('\nTest 3: Completely different frame (should send)');
const result3 = similarity.shouldSendFrame(testBuffer3, metadata);
console.log('Result:', result3, '(expected: true)\n');

console.log('\nTest 4: Same as previous (should NOT send)');
const result4 = similarity.shouldSendFrame(testBuffer3, metadata);
console.log('Result:', result4, '(expected: false)\n');

console.log('\nTest 5: Reset and test again');
similarity.reset();
const result5 = similarity.shouldSendFrame(testBuffer1, metadata);
console.log('Result:', result5, '(expected: true - after reset)\n');

console.log('\nFinal stats:', similarity.getStats());

const allPassed = result1 === true && result2 === false && result3 === true && result4 === false && result5 === true;
console.log(allPassed ? '\n✅ All tests PASSED' : '\n❌ Some tests FAILED');
