```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  26.62 μs |  9.651 μs | 0.529 μs |  1.00 |    0.02 |    2 |  0.1221 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 128.88 μs | 33.600 μs | 1.842 μs |  4.84 |    0.10 |    4 |  0.4883 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 574.97 μs | 85.880 μs | 4.707 μs | 21.61 |    0.40 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  16.51 μs |  0.371 μs | 0.020 μs |  0.62 |    0.01 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  71.44 μs | 35.331 μs | 1.937 μs |  2.69 |    0.08 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 365.80 μs | 15.609 μs | 0.856 μs | 13.75 |    0.24 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
