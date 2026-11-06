```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.213 ns |  1.162 ns | 0.0637 ns |  0.33 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   9.631 ns |  8.966 ns | 0.4915 ns |  1.00 |    0.06 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 558.152 ns | 21.549 ns | 1.1812 ns | 58.05 |    2.59 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 648.725 ns | 24.550 ns | 1.3456 ns | 67.47 |    3.01 | 0.0057 |     504 B |       12.60 |
