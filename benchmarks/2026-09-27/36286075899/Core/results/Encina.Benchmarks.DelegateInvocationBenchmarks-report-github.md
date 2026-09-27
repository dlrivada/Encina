```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.10 ns |   0.319 ns |   0.468 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.46 ns |   0.200 ns |   0.293 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,903.96 ns | 337.770 ns | 495.099 ns | 3,154.17 |   34.71 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    135.59 ns |   0.371 ns |   0.555 ns |     4.60 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     77.11 ns |   0.448 ns |   0.643 ns |     2.62 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
