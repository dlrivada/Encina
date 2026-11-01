```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.510 ns | 0.0561 ns | 0.0804 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.166 ns | 0.1108 ns | 0.1658 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   709.779 ns | 4.8460 ns | 7.1032 ns |  77.46 |    1.57 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,091.244 ns | 1.6217 ns | 2.2733 ns | 119.09 |    2.13 | 0.0286 |     504 B |       12.60 |
