```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.678 ns |  0.3055 ns | 0.0167 ns |   0.37 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.313 ns |  1.0821 ns | 0.0593 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 571.757 ns | 75.8675 ns | 4.1586 ns |  78.19 |    0.74 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 849.868 ns | 40.4799 ns | 2.2188 ns | 116.22 |    0.85 | 0.0296 |     504 B |       12.60 |
