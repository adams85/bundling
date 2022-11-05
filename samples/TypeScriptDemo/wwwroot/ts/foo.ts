// re-exports everything from bar
export * from './bar.js';

// exports a functions
function _timeout(ms: any) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export { _timeout as timeout };