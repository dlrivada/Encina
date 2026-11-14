```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.579 ns |   0.2624 ns |  0.0144 ns |  0.31 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.356 ns |   4.7048 ns |  0.2579 ns |  1.00 |    0.04 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 549.130 ns | 326.1397 ns | 17.8768 ns | 65.76 |    2.54 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 625.315 ns |   7.3211 ns |  0.4013 ns | 74.88 |    1.98 | 0.0057 |     504 B |       12.60 |
