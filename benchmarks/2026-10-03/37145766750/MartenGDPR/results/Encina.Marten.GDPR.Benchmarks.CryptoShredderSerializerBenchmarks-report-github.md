```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   522.1 ns | 147.06 ns |  8.06 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   551.5 ns |  62.02 ns |  3.40 ns |  1.06 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 7,251.5 ns | 833.74 ns | 45.70 ns | 13.89 |    0.20 |    2 | 0.1526 |    2637 B |        9.69 |
