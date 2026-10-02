```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   476.9 ns |   112.48 ns |   6.17 ns |  1.00 |    0.02 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   514.3 ns |    10.33 ns |   0.57 ns |  1.08 |    0.01 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 7,571.9 ns | 2,711.21 ns | 148.61 ns | 15.88 |    0.32 |    2 | 0.0305 |    2869 B |       10.55 |
