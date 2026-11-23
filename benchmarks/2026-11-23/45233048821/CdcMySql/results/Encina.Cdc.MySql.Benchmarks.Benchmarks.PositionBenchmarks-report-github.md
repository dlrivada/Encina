```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.593 ns |   0.1433 ns |  0.0079 ns |  0.30 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.561 ns |   6.8494 ns |  0.3754 ns |  1.00 |    0.05 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 598.388 ns | 807.3487 ns | 44.2535 ns | 69.99 |    5.24 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 626.963 ns |   5.6633 ns |  0.3104 ns | 73.33 |    2.83 | 0.0057 |     504 B |       12.60 |
