```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   604.2 ns |    11.45 ns |  0.63 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   651.1 ns |    14.31 ns |  0.78 ns |  1.08 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,151.5 ns | 1,073.79 ns | 58.86 ns | 15.15 |    0.09 |    2 | 0.1678 |    2869 B |       10.55 |
