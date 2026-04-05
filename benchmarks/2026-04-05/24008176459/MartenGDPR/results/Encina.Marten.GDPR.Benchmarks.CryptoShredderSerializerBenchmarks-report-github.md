```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                    | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|-------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| InnerSerializer_NonPii    | 33.35 ms |    NA |  1.00 |    1 |     272 B |        1.00 |
| CryptoSerializer_NonPii   | 35.43 ms |    NA |  1.06 |    2 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 62.63 ms |    NA |  1.88 |    3 |    4528 B |       16.65 |
