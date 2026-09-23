```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.656 ns |   0.0561 ns |  0.0031 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.641 ns |   3.7360 ns |  0.2048 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 561.995 ns |  53.8577 ns |  2.9521 ns |  84.67 |    2.30 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 862.815 ns | 322.7835 ns | 17.6929 ns | 130.00 |    4.17 | 0.0296 |     504 B |       12.60 |
