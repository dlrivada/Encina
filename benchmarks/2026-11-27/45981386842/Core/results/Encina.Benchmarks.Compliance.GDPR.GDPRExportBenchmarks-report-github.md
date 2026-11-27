```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.41 μs |   2.547 μs | 0.140 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 114.53 μs |   1.857 μs | 0.102 μs |  4.69 |    0.02 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 540.36 μs | 100.856 μs | 5.528 μs | 22.13 |    0.22 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  12.13 μs |   6.385 μs | 0.350 μs |  0.50 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  53.93 μs |  25.426 μs | 1.394 μs |  2.21 |    0.05 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 345.09 μs | 156.365 μs | 8.571 μs | 14.14 |    0.31 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
