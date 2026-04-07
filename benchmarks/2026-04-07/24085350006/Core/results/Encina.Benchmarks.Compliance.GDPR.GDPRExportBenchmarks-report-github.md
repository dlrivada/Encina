```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.23 μs |   1.224 μs |  0.067 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 164.02 μs |  11.042 μs |  0.605 μs |  4.66 |    0.02 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 759.60 μs |  86.239 μs |  4.727 μs | 21.56 |    0.12 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  18.50 μs |   2.066 μs |  0.113 μs |  0.53 |    0.00 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  81.91 μs |   7.505 μs |  0.411 μs |  2.32 |    0.01 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 483.35 μs | 231.177 μs | 12.672 μs | 13.72 |    0.31 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
