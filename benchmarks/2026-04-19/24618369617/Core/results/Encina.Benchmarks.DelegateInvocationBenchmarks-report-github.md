```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.15 ns |   0.750 ns |   1.100 ns |     1.31 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.26 ns |   0.144 ns |   0.216 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,653.37 ns | 597.539 ns | 837.666 ns | 3,208.31 |   37.84 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    129.55 ns |   0.838 ns |   1.228 ns |     4.58 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     88.39 ns |   0.485 ns |   0.710 ns |     3.13 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
