```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.05 μs |  2.269 μs | 0.124 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 172.18 μs |  5.006 μs | 0.274 μs |  4.91 |    0.02 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 753.99 μs | 39.560 μs | 2.168 μs | 21.51 |    0.09 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  17.71 μs |  3.167 μs | 0.174 μs |  0.51 |    0.00 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  78.93 μs | 20.417 μs | 1.119 μs |  2.25 |    0.03 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 441.61 μs | 48.513 μs | 2.659 μs | 12.60 |    0.08 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
