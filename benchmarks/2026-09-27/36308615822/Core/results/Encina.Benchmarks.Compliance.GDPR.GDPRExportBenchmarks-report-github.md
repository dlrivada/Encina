```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  29.16 μs |   3.231 μs | 0.177 μs |  1.00 |    0.01 |    2 |  0.1221 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 138.80 μs |  15.096 μs | 0.827 μs |  4.76 |    0.04 |    4 |  0.4883 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 621.25 μs | 130.640 μs | 7.161 μs | 21.30 |    0.24 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  17.11 μs |   1.270 μs | 0.070 μs |  0.59 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  77.80 μs |   7.290 μs | 0.400 μs |  2.67 |    0.02 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 393.06 μs | 148.978 μs | 8.166 μs | 13.48 |    0.25 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
