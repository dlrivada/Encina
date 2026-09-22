```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   482.7 ns |    22.82 ns |   1.25 ns |  1.00 |    0.00 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   496.0 ns |    70.48 ns |   3.86 ns |  1.03 |    0.01 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 7,554.7 ns | 2,581.17 ns | 141.48 ns | 15.65 |    0.26 |    2 | 0.0305 |    2869 B |       10.55 |
