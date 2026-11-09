```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.494 ns |   1.8624 ns |  0.1021 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.657 ns |   0.6020 ns |  0.0330 ns |   1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 684.655 ns |  20.4509 ns |  1.1210 ns |  89.42 |    0.36 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 810.159 ns | 228.5401 ns | 12.5271 ns | 105.81 |    1.47 | 0.0057 |     504 B |       12.60 |
