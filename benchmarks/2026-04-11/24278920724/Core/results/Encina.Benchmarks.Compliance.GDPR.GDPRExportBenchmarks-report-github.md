```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  31.21 μs | 0.089 μs | 0.122 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 144.92 μs | 0.276 μs | 0.395 μs |  4.64 |    0.02 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 697.85 μs | 3.378 μs | 4.736 μs | 22.36 |    0.17 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  14.38 μs | 0.042 μs | 0.060 μs |  0.46 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  64.06 μs | 0.355 μs | 0.520 μs |  2.05 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 415.24 μs | 4.677 μs | 6.402 μs | 13.30 |    0.21 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
