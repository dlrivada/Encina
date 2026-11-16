```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.654 ns |   0.2662 ns | 0.0146 ns |  0.38 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   9.716 ns |   1.6554 ns | 0.0907 ns |  1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 794.675 ns |  51.5743 ns | 2.8270 ns | 81.79 |    0.71 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 947.125 ns | 143.7394 ns | 7.8788 ns | 97.48 |    1.05 | 0.0057 |     504 B |       12.60 |
