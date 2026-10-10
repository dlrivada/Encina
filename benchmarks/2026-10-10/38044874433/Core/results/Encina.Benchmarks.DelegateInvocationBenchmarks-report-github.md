```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.48 ns |     7.414 ns |   0.406 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.91 ns |     0.573 ns |   0.031 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,819.17 ns | 3,689.744 ns | 202.247 ns | 2,410.53 |    8.53 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    108.85 ns |    12.713 ns |   0.697 ns |     4.97 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     68.01 ns |     4.353 ns |   0.239 ns |     3.10 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
