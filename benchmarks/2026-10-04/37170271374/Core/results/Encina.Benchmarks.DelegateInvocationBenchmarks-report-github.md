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
| CompiledDelegate        |     38.73 ns |   0.427 ns |   0.640 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.76 ns |   0.218 ns |   0.326 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,550.07 ns | 254.662 ns | 381.166 ns | 2,976.38 |   33.34 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    138.68 ns |   0.259 ns |   0.372 ns |     4.51 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.38 ns |   0.560 ns |   0.821 ns |     2.61 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
