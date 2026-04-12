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
| CompiledDelegate        |     37.21 ns |   0.452 ns |   0.677 ns |     0.97 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     38.29 ns |   0.689 ns |   1.032 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,654.10 ns | 251.866 ns | 361.219 ns | 2,369.28 |   64.20 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    128.68 ns |   1.472 ns |   2.204 ns |     3.36 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.17 ns |   0.543 ns |   0.778 ns |     2.12 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
