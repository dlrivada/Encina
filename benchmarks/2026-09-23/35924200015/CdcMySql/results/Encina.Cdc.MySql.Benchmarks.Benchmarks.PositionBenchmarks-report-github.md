```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.425 ns |   0.0651 ns |  0.0036 ns |   0.36 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.415 ns |   3.1986 ns |  0.1753 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   713.209 ns | 226.8030 ns | 12.4318 ns |  75.77 |    1.66 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,061.339 ns |  56.8659 ns |  3.1170 ns | 112.75 |    1.82 | 0.0286 |     504 B |       12.60 |
