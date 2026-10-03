```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.802 ns |  0.9316 ns | 0.0511 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.185 ns |  0.6197 ns | 0.0340 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 412.083 ns | 55.2697 ns | 3.0295 ns |  98.47 |    0.94 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 680.042 ns |  8.9813 ns | 0.4923 ns | 162.51 |    1.15 | 0.0296 |     504 B |       12.60 |
