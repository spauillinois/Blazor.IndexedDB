import { IndexedDbManager } from './indexedDbBlazor.js';
var IndexDb;
(function (IndexDb) {
    const timeghostExtensions = 'TimeGhost';
    const extensionObject = {
        IndexedDbManager: new IndexedDbManager()
    };
    function initialise() {
        if (typeof window !== 'undefined' && !window[timeghostExtensions]) {
            window[timeghostExtensions] = Object.assign({}, extensionObject);
        }
        else {
            window[timeghostExtensions] = Object.assign(Object.assign({}, window[timeghostExtensions]), extensionObject);
        }
    }
    IndexDb.initialise = initialise;
})(IndexDb || (IndexDb = {}));
IndexDb.initialise();