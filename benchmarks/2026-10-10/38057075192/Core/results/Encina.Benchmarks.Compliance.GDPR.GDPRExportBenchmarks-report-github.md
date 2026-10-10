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
| &#39;JSON export: 10 activities&#39;  |  28.41 μs |   0.798 μs | 0.044 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 135.38 μs |   8.451 μs | 0.463 μs |  4.77 |    0.02 |    4 |  0.4883 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 616.22 μs |  63.393 μs | 3.475 μs | 21.69 |    0.11 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  15.41 μs |   1.891 μs | 0.104 μs |  0.54 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  70.13 μs |   4.346 μs | 0.238 μs |  2.47 |    0.01 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 377.36 μs | 124.065 μs | 6.800 μs | 13.29 |    0.21 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
