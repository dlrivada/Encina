```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.577 ns | 0.0060 ns | 0.0084 ns |   0.33 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.723 ns | 0.0927 ns | 0.1387 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 395.440 ns | 3.0902 ns | 4.3320 ns |  83.80 |    2.56 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 616.206 ns | 5.3189 ns | 7.7963 ns | 130.58 |    4.07 | 0.0296 |     504 B |       12.60 |
