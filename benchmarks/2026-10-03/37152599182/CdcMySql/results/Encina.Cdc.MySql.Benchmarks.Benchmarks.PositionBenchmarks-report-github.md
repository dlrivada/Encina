```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.429 ns |   0.2003 ns | 0.0110 ns |   0.36 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.417 ns |   3.2745 ns | 0.1795 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   709.892 ns |  78.2823 ns | 4.2909 ns |  75.40 |    1.32 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,067.814 ns | 144.4254 ns | 7.9164 ns | 113.42 |    2.02 | 0.0286 |     504 B |       12.60 |
