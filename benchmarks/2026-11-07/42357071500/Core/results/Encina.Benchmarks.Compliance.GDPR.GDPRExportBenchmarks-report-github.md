```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.48GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  34.64 μs |  1.782 μs | 0.098 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 163.90 μs | 11.808 μs | 0.647 μs |  4.73 |    0.02 |    4 |  2.1973 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 761.80 μs | 66.943 μs | 3.669 μs | 21.99 |    0.11 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  18.93 μs |  3.945 μs | 0.216 μs |  0.55 |    0.01 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  83.45 μs | 10.786 μs | 0.591 μs |  2.41 |    0.02 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 489.37 μs | 49.976 μs | 2.739 μs | 14.13 |    0.08 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
