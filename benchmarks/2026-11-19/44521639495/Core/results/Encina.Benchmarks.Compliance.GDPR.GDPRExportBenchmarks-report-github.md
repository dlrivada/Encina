```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  21.62 μs |  31.782 μs | 1.742 μs |  1.00 |    0.10 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  97.90 μs |   1.427 μs | 0.078 μs |  4.55 |    0.30 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 509.32 μs | 130.013 μs | 7.126 μs | 23.65 |    1.60 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.12 KB |       18.18 |
| &#39;CSV export: 10 activities&#39;   |  10.96 μs |   0.680 μs | 0.037 μs |  0.51 |    0.03 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  49.88 μs |   8.236 μs | 0.451 μs |  2.32 |    0.16 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 286.98 μs | 102.393 μs | 5.613 μs | 13.33 |    0.92 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
