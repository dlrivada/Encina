```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  34.74 μs |   2.144 μs | 0.118 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 164.42 μs |  51.160 μs | 2.804 μs |  4.73 |    0.07 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 723.54 μs |  24.722 μs | 1.355 μs | 20.83 |    0.07 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  17.91 μs |   1.390 μs | 0.076 μs |  0.52 |    0.00 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  80.02 μs |  12.116 μs | 0.664 μs |  2.30 |    0.02 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 427.17 μs | 113.622 μs | 6.228 μs | 12.30 |    0.16 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
