/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
var showControllersOnly = false;
var seriesFilter = "";
var filtersOnlySampleSeries = true;

/*
 * Add header in statistics table to group metrics by category
 * format
 *
 */
function summaryTableHeader(header) {
    var newRow = header.insertRow(-1);
    newRow.className = "tablesorter-no-sort";
    var cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 1;
    cell.innerHTML = "Requests";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 3;
    cell.innerHTML = "Executions";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 7;
    cell.innerHTML = "Response Times (ms)";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 2;
    cell.innerHTML = "Network (KB/sec)";
    newRow.appendChild(cell);
}

/*
 * Populates the table identified by id parameter with the specified data and
 * format
 *
 */
function createTable(table, info, formatter, defaultSorts, seriesIndex, headerCreator) {
    var tableRef = table[0];

    // Create header and populate it with data.titles array
    var header = tableRef.createTHead();

    // Call callback is available
    if(headerCreator) {
        headerCreator(header);
    }

    var newRow = header.insertRow(-1);
    for (var index = 0; index < info.titles.length; index++) {
        var cell = document.createElement('th');
        cell.innerHTML = info.titles[index];
        newRow.appendChild(cell);
    }

    var tBody;

    // Create overall body if defined
    if(info.overall){
        tBody = document.createElement('tbody');
        tBody.className = "tablesorter-no-sort";
        tableRef.appendChild(tBody);
        var newRow = tBody.insertRow(-1);
        var data = info.overall.data;
        for(var index=0;index < data.length; index++){
            var cell = newRow.insertCell(-1);
            cell.innerHTML = formatter ? formatter(index, data[index]): data[index];
        }
    }

    // Create regular body
    tBody = document.createElement('tbody');
    tableRef.appendChild(tBody);

    var regexp;
    if(seriesFilter) {
        regexp = new RegExp(seriesFilter, 'i');
    }
    // Populate body with data.items array
    for(var index=0; index < info.items.length; index++){
        var item = info.items[index];
        if((!regexp || filtersOnlySampleSeries && !info.supportsControllersDiscrimination || regexp.test(item.data[seriesIndex]))
                &&
                (!showControllersOnly || !info.supportsControllersDiscrimination || item.isController)){
            if(item.data.length > 0) {
                var newRow = tBody.insertRow(-1);
                for(var col=0; col < item.data.length; col++){
                    var cell = newRow.insertCell(-1);
                    cell.innerHTML = formatter ? formatter(col, item.data[col]) : item.data[col];
                }
            }
        }
    }

    // Add support of columns sort
    table.tablesorter({sortList : defaultSorts});
}

$(document).ready(function() {

    // Customize table sorter default options
    $.extend( $.tablesorter.defaults, {
        theme: 'blue',
        cssInfoBlock: "tablesorter-no-sort",
        widthFixed: true,
        widgets: ['zebra']
    });

    var data = {"OkPercent": 99.99571750036371, "KoPercent": 0.004282499636295454};
    var dataset = [
        {
            "label" : "KO",
            "data" : data.KoPercent,
            "color" : "#FF6347"
        },
        {
            "label" : "OK",
            "data" : data.OkPercent,
            "color" : "#9ACD32"
        }];
    $.plot($("#flot-requests-summary"), dataset, {
        series : {
            pie : {
                show : true,
                radius : 1,
                label : {
                    show : true,
                    radius : 3 / 4,
                    formatter : function(label, series) {
                        return '<div style="font-size:8pt;text-align:center;padding:2px;color:white;">'
                            + label
                            + '<br/>'
                            + Math.round10(series.percent, -2)
                            + '%</div>';
                    },
                    background : {
                        opacity : 0.5,
                        color : '#000'
                    }
                }
            }
        },
        legend : {
            show : true
        }
    });

    // Creates APDEX table
    createTable($("#apdexTable"), {"supportsControllersDiscrimination": true, "overall": {"data": [0.9939886069402928, 500, 1500, "Total"], "isController": false}, "titles": ["Apdex", "T (Toleration threshold)", "F (Frustration threshold)", "Label"], "items": [{"data": [0.9940250931684081, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 2 paths"], "isController": false}, {"data": [0.993982855852285, 500, 1500, "ACME - uriinfo - URL in 40M w/ 5 paths & querystring"], "isController": false}, {"data": [0.9939054999677965, 500, 1500, "ACME - uriinfo - URL in 40M w/ 1 path & query string"], "isController": false}, {"data": [0.9940410254193981, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 5 paths"], "isController": false}]}, function(index, item){
        switch(index){
            case 0:
                item = item.toFixed(3);
                break;
            case 1:
            case 2:
                item = formatDuration(item);
                break;
        }
        return item;
    }, [[0, 0]], 3);

    // Create statistics table
    createTable($("#statisticsTable"), {"supportsControllersDiscrimination": true, "overall": {"data": ["Total", 21109167, 904, 0.004282499636295454, 95.55030409299354, 0, 23014, 318.0, 367.9500000000007, 792.9900000000016, 5858.4825071187115, 4372.957570747402, 3090.0646020060753], "isController": false}, "titles": ["Label", "#Samples", "KO", "Error %", "Average", "Min", "Max", "90th pct", "95th pct", "99th pct", "Throughput", "Received", "Sent"], "items": [{"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 5276735, 223, 0.004226098145917883, 95.48823732856806, 0, 21011, 299.0, 355.0, 626.9900000000016, 1464.689278474537, 977.4060805318392, 743.8502165804312], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 5277836, 198, 0.003751537561985632, 95.50311112356603, 0, 21015, 299.0, 357.0, 616.0, 1464.965203913505, 1247.3213722985915, 811.4292867549971], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 5278940, 246, 0.004660026444702914, 95.8089930933012, 0, 23011, 300.0, 355.0, 604.9900000000016, 1465.07806992712, 959.8696688367984, 738.5200735986164], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 5275656, 237, 0.004492332327960731, 95.40074599253197, 0, 23014, 303.0, 361.0, 612.9900000000016, 1464.386116847645, 1188.8523204727992, 796.6049416951007], "isController": false}]}, function(index, item){
        switch(index){
            // Errors pct
            case 3:
                item = item.toFixed(2) + '%';
                break;
            // Mean
            case 4:
            // Mean
            case 7:
            // Percentile 1
            case 8:
            // Percentile 2
            case 9:
            // Percentile 3
            case 10:
            // Throughput
            case 11:
            // Kbytes/s
            case 12:
            // Sent Kbytes/s
                item = item.toFixed(2);
                break;
        }
        return item;
    }, [[0, 0]], 0, summaryTableHeader);

    // Create error table
    createTable($("#errorsTable"), {"supportsControllersDiscrimination": false, "titles": ["Type of error", "Number of errors", "% in errors", "% in all samples"], "items": [{"data": ["Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 11, 1.2168141592920354, 5.2110061946073E-5], "isController": false}, {"data": ["Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 893, 98.78318584070796, 0.004230389574349381], "isController": false}]}, function(index, item){
        switch(index){
            case 2:
            case 3:
                item = item.toFixed(2) + '%';
                break;
        }
        return item;
    }, [[1, 1]]);

        // Create top5 errors by sampler
    createTable($("#top5ErrorsBySamplerTable"), {"supportsControllersDiscrimination": false, "overall": {"data": ["Total", 21109167, 904, "Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 893, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 11, null, null, null, null, null, null], "isController": false}, "titles": ["Sample", "#Samples", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors"], "items": [{"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 5276735, 223, "Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 221, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 2, null, null, null, null, null, null], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 5277836, 198, "Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 194, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 4, null, null, null, null, null, null], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 5278940, 246, "Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 244, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 2, null, null, null, null, null, null], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 5275656, 237, "Non HTTP response code: java.net.BindException/Non HTTP response message: Address already in use: connect", 234, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 3, null, null, null, null, null, null], "isController": false}]}, function(index, item){
        return item;
    }, [[0, 0]], 0);

});
