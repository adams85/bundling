// re-exports everything from bar
export * from './bar.js';
// exports a functions
function _timeout(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
export { _timeout as timeout };
//# sourceMappingURL=foo.js.map