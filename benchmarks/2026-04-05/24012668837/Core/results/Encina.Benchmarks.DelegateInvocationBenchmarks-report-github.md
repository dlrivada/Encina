```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                  | Mean       | Error | Ratio | Allocated | Alloc Ratio |
|------------------------ |-----------:|------:|------:|----------:|------------:|
| DirectCall              | 5,299.4 μs |    NA |  1.00 |     112 B |        1.00 |
| CompiledDelegate        | 5,296.5 μs |    NA |  1.00 |     112 B |        1.00 |
| MethodInfoInvoke        | 5,558.9 μs |    NA |  1.05 |    2680 B |       23.93 |
| GenericTypeConstruction | 5,457.0 μs |    NA |  1.03 |    1032 B |        9.21 |
| ExpressionCompilation   |   724.6 μs |    NA |  0.14 |    5912 B |       52.79 |
