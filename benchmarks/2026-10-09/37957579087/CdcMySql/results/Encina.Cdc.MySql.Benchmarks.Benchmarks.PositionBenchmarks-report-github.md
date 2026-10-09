```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.678 ns |  0.3428 ns | 0.0188 ns |   0.41 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.590 ns |  2.3014 ns | 0.1261 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 553.000 ns | 30.5334 ns | 1.6736 ns |  83.93 |    1.42 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 835.817 ns | 83.6945 ns | 4.5876 ns | 126.86 |    2.20 | 0.0296 |     504 B |       12.60 |
