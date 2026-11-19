```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.37 μs |  1.843 μs | 0.101 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 114.48 μs |  1.024 μs | 0.056 μs |  4.70 |    0.02 |    4 |  3.2959 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 534.42 μs |  7.296 μs | 0.400 μs | 21.93 |    0.08 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  11.20 μs |  3.815 μs | 0.209 μs |  0.46 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  49.77 μs | 18.909 μs | 1.036 μs |  2.04 |    0.04 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 317.07 μs | 31.308 μs | 1.716 μs | 13.01 |    0.08 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
