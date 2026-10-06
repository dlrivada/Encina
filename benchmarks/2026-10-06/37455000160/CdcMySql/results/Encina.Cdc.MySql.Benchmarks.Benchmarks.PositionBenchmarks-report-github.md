```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.377 ns |   0.2998 ns |  0.0164 ns |   0.36 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.350 ns |   1.7919 ns |  0.0982 ns |   1.00 |    0.01 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   870.407 ns | 327.1093 ns | 17.9300 ns |  93.10 |    1.86 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,202.856 ns |  16.4110 ns |  0.8995 ns | 128.66 |    1.17 | 0.0191 |     504 B |       12.60 |
