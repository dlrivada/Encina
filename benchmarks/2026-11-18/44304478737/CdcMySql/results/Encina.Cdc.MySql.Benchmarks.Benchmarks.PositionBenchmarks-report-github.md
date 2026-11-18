```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.913 ns |   0.7650 ns | 0.0419 ns |  0.36 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.146 ns |   2.2983 ns | 0.1260 ns |  1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 550.821 ns |  12.8294 ns | 0.7032 ns | 67.63 |    0.90 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 650.831 ns | 150.8599 ns | 8.2691 ns | 79.91 |    1.38 | 0.0057 |     504 B |       12.60 |
