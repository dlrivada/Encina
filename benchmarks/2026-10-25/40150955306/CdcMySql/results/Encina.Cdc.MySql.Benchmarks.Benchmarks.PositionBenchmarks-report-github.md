```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.659 ns | 0.0040 ns | 0.0054 ns |   0.35 |    0.03 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.704 ns | 0.3915 ns | 0.5860 ns |   1.01 |    0.11 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 559.881 ns | 1.9273 ns | 2.8847 ns |  73.08 |    5.44 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 842.135 ns | 1.8486 ns | 2.7097 ns | 109.92 |    8.17 | 0.0296 |     504 B |       12.60 |
