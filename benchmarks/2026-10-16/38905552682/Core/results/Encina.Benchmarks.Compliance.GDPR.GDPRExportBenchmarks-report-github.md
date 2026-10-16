```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  29.31 μs |   1.664 μs |  0.091 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 140.04 μs |  10.865 μs |  0.596 μs |  4.78 |    0.02 |    4 |  0.4883 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 663.01 μs |  54.834 μs |  3.006 μs | 22.62 |    0.11 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  17.70 μs |   0.940 μs |  0.052 μs |  0.60 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  81.79 μs |  46.458 μs |  2.547 μs |  2.79 |    0.08 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 444.97 μs | 309.710 μs | 16.976 μs | 15.18 |    0.50 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
