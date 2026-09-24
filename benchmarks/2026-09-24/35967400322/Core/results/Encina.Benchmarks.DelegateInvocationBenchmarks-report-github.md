```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     34.79 ns |      2.639 ns |   0.145 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.95 ns |      3.707 ns |   0.203 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,340.44 ns | 10,864.917 ns | 595.543 ns | 2,337.67 |   23.56 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    127.56 ns |      2.844 ns |   0.156 ns |     4.56 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     87.77 ns |      4.209 ns |   0.231 ns |     3.14 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
