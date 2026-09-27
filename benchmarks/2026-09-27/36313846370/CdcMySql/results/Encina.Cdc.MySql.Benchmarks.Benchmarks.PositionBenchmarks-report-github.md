```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.656 ns |  0.0282 ns | 0.0015 ns |   0.42 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.392 ns |  0.8922 ns | 0.0489 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 551.564 ns | 67.7662 ns | 3.7145 ns |  86.29 |    0.76 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 830.173 ns | 15.5686 ns | 0.8534 ns | 129.88 |    0.87 | 0.0296 |     504 B |       12.60 |
