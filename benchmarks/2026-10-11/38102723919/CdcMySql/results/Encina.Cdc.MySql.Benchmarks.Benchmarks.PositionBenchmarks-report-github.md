```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.975 ns | 0.0039 ns | 0.0055 ns |   0.44 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.017 ns | 0.1593 ns | 0.2385 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   794.205 ns | 5.2461 ns | 7.5239 ns |  88.14 |    2.47 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,138.833 ns | 1.9516 ns | 2.8607 ns | 126.38 |    3.36 | 0.0286 |     504 B |       12.60 |
