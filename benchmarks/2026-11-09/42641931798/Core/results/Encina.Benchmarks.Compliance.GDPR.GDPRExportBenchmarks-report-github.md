```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  19.296 μs |  17.974 μs |  0.9852 μs |  1.00 |    0.06 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  86.808 μs |  12.691 μs |  0.6956 μs |  4.51 |    0.20 |    4 |  3.2959 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 408.193 μs | 301.923 μs | 16.5494 μs | 21.19 |    1.18 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   8.276 μs |   5.809 μs |  0.3184 μs |  0.43 |    0.02 |    1 |  3.3569 |  0.2365 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  34.452 μs |  12.723 μs |  0.6974 μs |  1.79 |    0.08 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 241.535 μs | 172.339 μs |  9.4465 μs | 12.54 |    0.69 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
