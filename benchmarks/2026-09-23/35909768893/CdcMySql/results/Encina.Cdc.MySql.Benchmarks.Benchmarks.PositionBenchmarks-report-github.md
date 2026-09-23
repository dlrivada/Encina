```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.046 ns |  0.1750 ns | 0.0096 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.701 ns |  2.5042 ns | 0.1373 ns |   1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 689.560 ns | 20.5217 ns | 1.1249 ns |  89.56 |    1.37 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 774.941 ns | 54.7492 ns | 3.0010 ns | 100.65 |    1.57 | 0.0057 |     504 B |       12.60 |
