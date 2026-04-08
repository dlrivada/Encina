```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  31.74 μs | 0.086 μs |  0.124 μs |  31.74 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 157.51 μs | 5.800 μs |  8.318 μs | 164.66 μs |  4.96 |    0.26 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 712.92 μs | 0.729 μs |  1.045 μs | 712.83 μs | 22.46 |    0.09 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  14.81 μs | 0.125 μs |  0.186 μs |  14.83 μs |  0.47 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  64.48 μs | 0.312 μs |  0.438 μs |  64.63 μs |  2.03 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 416.99 μs | 8.367 μs | 11.999 μs | 417.13 μs | 13.14 |    0.37 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
