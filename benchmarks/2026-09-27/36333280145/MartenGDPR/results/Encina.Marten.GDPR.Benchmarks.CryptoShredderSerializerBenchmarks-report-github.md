```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   426.9 ns |  29.83 ns |  1.64 ns |  1.00 |    0.00 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   442.8 ns |  44.41 ns |  2.43 ns |  1.04 |    0.01 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 6,672.4 ns | 231.60 ns | 12.70 ns | 15.63 |    0.06 |    2 | 0.0305 |    2869 B |       10.55 |
