```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.948 ns |   0.5830 ns | 0.0320 ns |  0.39 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |  10.183 ns |   2.0178 ns | 0.1106 ns |  1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 777.533 ns |  15.6334 ns | 0.8569 ns | 76.36 |    0.73 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 915.766 ns | 101.9836 ns | 5.5901 ns | 89.93 |    0.97 | 0.0057 |     504 B |       12.60 |
