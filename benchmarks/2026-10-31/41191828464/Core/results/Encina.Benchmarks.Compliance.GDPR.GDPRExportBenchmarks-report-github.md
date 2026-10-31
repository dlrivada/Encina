```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.51 μs |  1.638 μs | 0.090 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 120.91 μs |  4.327 μs | 0.237 μs |  4.93 |    0.02 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 597.06 μs |  7.420 μs | 0.407 μs | 24.36 |    0.08 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  11.06 μs |  1.137 μs | 0.062 μs |  0.45 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  50.32 μs |  4.813 μs | 0.264 μs |  2.05 |    0.01 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 332.91 μs | 15.217 μs | 0.834 μs | 13.58 |    0.05 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
