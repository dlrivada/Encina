```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.20 ns |   0.300 ns |   0.450 ns |     1.27 |    0.05 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.78 ns |   0.878 ns |   1.286 ns |     1.00 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,191.27 ns | 313.952 ns | 450.261 ns | 2,905.20 |  119.23 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    126.92 ns |   0.977 ns |   1.463 ns |     4.00 |    0.17 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.93 ns |   0.818 ns |   1.173 ns |     2.64 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
