```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.716 ns |   0.1163 ns |  0.0064 ns |   0.31 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.542 ns |   3.5081 ns |  0.1923 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 463.549 ns | 367.5274 ns | 20.1454 ns |  83.71 |    4.03 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 683.778 ns |  93.6162 ns |  5.1314 ns | 123.47 |    3.78 | 0.0296 |     504 B |       12.60 |
