```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.635 ns |  1.1706 ns | 0.0642 ns |   0.31 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.283 ns |  0.9321 ns | 0.0511 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 422.608 ns | 37.8498 ns | 2.0747 ns |  80.00 |    0.75 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 648.868 ns | 16.2068 ns | 0.8883 ns | 122.84 |    1.04 | 0.0296 |     504 B |       12.60 |
