```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.61 μs |   0.694 μs |  0.038 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 163.42 μs |   2.459 μs |  0.135 μs |  4.59 |    0.01 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 752.29 μs |  48.013 μs |  2.632 μs | 21.12 |    0.07 |    6 | 51.7578 | 51.7578 | 51.7578 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  15.59 μs |   6.686 μs |  0.367 μs |  0.44 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  69.00 μs |  34.597 μs |  1.896 μs |  1.94 |    0.05 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 410.51 μs | 197.176 μs | 10.808 μs | 11.53 |    0.26 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
