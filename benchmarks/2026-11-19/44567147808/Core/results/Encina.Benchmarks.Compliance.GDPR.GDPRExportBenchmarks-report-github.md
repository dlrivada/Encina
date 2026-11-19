```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  31.88 μs |   7.991 μs |  0.438 μs |  1.00 |    0.02 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 148.81 μs |   2.932 μs |  0.161 μs |  4.67 |    0.06 |    4 |  3.1738 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 744.00 μs |  30.442 μs |  1.669 μs | 23.34 |    0.28 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  16.64 μs |   4.179 μs |  0.229 μs |  0.52 |    0.01 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  71.75 μs |  12.619 μs |  0.692 μs |  2.25 |    0.03 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 469.08 μs | 373.206 μs | 20.457 μs | 14.72 |    0.58 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
