```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.39 ns |     5.101 ns |   0.280 ns |     1.23 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.49 ns |    11.365 ns |   0.623 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,691.54 ns | 7,053.269 ns | 386.613 ns | 3,177.61 |   58.72 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.95 ns |    10.330 ns |   0.566 ns |     4.61 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     77.84 ns |     9.061 ns |   0.497 ns |     2.64 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
